namespace BoxBounce

open System
open System.Drawing
open Rhino
open Rhino.Input
open Rhino.Geometry
open Rhino.Commands
open Microsoft.FSharp.Collections
open GosLib
open Rhino.Display
module rs = GosLib.RhinoScriptSyntax

module Core = 
    let cancel = ref false
    let rad = 0.07
    let speed = 0.005
    let box = Box(Plane.WorldXY, [Point3d(0.,0.,0.) ; Point3d(1.,1.,1.)] )
    let mat = new DisplayMaterial (new Rhino.DocObjects.Material())
    let msp = Mesh.CreateFromSphere(Sphere(Plane.WorldXY.Origin,rad),15,15)
    let rnd = new Random()
    type Ball = {mutable cen:Point3d ; sph: Mesh; ln:Geometry.Line; col : Color}

    let v3f (v:Vector3d) = Vector3f(float32 v.X, float32 v.Y, float32 v.Z)
    let p3f (v:Point3d) = Point3f(float32 v.X, float32 v.Y, float32 v.Z)

    let moveMesh (m:Mesh) (v:Vector3d) = 
        let v = v3f v
        for i = 0 to m.Vertices.Count-1 do
            m.Vertices.[i] <- m.Vertices.[i] + v
    
    let updateBall (ball:Ball) =
        let v = ball.ln.Direction
        v.Unitize() |> ignore
        let v = v * speed * ball.ln.Length 
        let cen = ball.cen + v
        if ball.ln.DistanceTo(cen,true) > 0.01 then
            ball.ln.Flip()
            ball.cen <- ball.cen - v
            moveMesh ball.sph (-v)
        else
            ball.cen <- cen
            moveMesh ball.sph v
    
    let getCamline(v:RhinoViewport)  = 
        let ln = Line(v.CameraLocation , v.CameraDirection )           
        ln.Extend(1000.,1000.) |> ignore
        let res,iv = Intersect.Intersection.LineBox(ln,box ,0.01)
        if res then 
            let ln = Line(ln.PointAt iv.T0 , ln.PointAt iv.T1)
            if ln.Length > 0.2 then Some ln
            else                    None
        else 
            None
    
    let getCrosshair (v:RhinoViewport)  = 
        match getCamline v with
        | Some ln -> 
            let x = v.CameraX
            x.Unitize() |> ignore
            let x = x*rad
            let y = v.CameraY
            y.Unitize() |> ignore
            let y = y*rad
            let cen = ln.From
            Some(Line(cen-x,cen+x),Line(cen-y,cen+y))
        | None -> None
      

    type Conduit () =
        inherit Rhino.Display.DisplayConduit()
        let balls = Rarr()

        member this.AddBall  (v:RhinoViewport) =         
            match getCamline v with
            | Some ln -> 
                let sph  = msp.DuplicateMesh() 
                moveMesh sph (ln.From - Plane.WorldXY.Origin)
                balls.Add {cen = ln.From ;sph = sph; ln = ln; col = Color.Red}
            | None -> ()
        
        override this.PreDrawObjects (e:DrawEventArgs) =                
            base.PreDrawObjects(e)

            match getCrosshair e.Viewport with
            |Some( ln1,ln2) ->
                e.Display.DrawLine (ln2,Color.Blue)
                e.Display.DrawLine (ln1,Color.Blue)
            |None -> ()

            let explode = Rarr()
            for i=0 to balls.Count-1 do
                let pt = balls.[i].cen
                for j=i+1 to balls.Count-1 do
                    let cen = balls.[j].cen
                    if (pt-cen).Length < rad * 2. then
                        explode.Add balls.[i] 
                        explode.Add balls.[j]
            
            if explode.Count > 0 then
                for ball in explode do  
                    ball.sph.Unweld(0.,true)
                    
                    let mutable cen = Point3d()
                    for p in  ball.sph.Vertices do
                        cen <- cen + (Point3d( p))
                    cen <- cen / float  ball.sph.Vertices.Count 
                      
                    for face in ball.sph.Faces do
                        let a = Point3d(ball.sph.Vertices.[face.A])
                        let b = Point3d(ball.sph.Vertices.[face.B])
                        let c = Point3d(ball.sph.Vertices.[face.C])
                        let d = Point3d(ball.sph.Vertices.[face.D])
                        let fc = (a+b+c+d) / 4.0
                        
                        let dir = (fc - cen) * 0.02
                        let ran() = (Vector3d(rnd.NextDouble(), rnd.NextDouble(), rnd.NextDouble()))*0.005                        
                        ball.sph.Vertices.[face.A] <- p3f (a + dir + ran() )
                        ball.sph.Vertices.[face.B] <- p3f (b + dir + ran() )
                        ball.sph.Vertices.[face.C] <- p3f (c + dir + ran() )
                        ball.sph.Vertices.[face.D] <- p3f (d + dir + ran() )

                        if abs(a.X) > 4.0 then 
                            balls.Clear()
                            cancel := true
                
                let cor = Point2d(550.,290.)
                let tx = "Game Over"
                //e.Display.Draw2dText(tx,Color.Red,cor,true)
                e.Display.Draw2dText(tx,Color.Red,cor,true, 90)
                    
            else
                for ball in balls do
                    updateBall ball

            for ball in balls do
                e.Display.DrawDottedLine (ball.ln  , ball.col) 
                e.Display.DrawMeshShaded (ball.sph , mat)   

            let cor = Point2d(150.,150.)
            let tx = sprintf "Lines: %d" balls.Count
            //e.Display.Draw2dText(tx,Color.Red,cor,true)
            e.Display.Draw2dText(tx,Color.Gray,cor,true, 50)
            e.Display.EnableDepthWriting true
            e.Display.EnableDepthTesting true
        
        member this.PDO (e:DrawEventArgs) = this.PreDrawObjects e

        
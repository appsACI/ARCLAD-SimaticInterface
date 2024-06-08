net use \\192.168.0.225\admin$ Siemens248 /user:Administrator
sc \\192.168.0.225 stop simaticarcorwebapi
robocopy "C:\Repositorios\Arcor\SimaticArcorWebApi\SimaticArcorWebApi\bin\Debug\netcoreapp2.2\publish\Demo" "\\192.168.0.225\c$\Program Files\Siemens\SimaticArcorInterface" /e /zb /copyall /is /it 
sc \\192.168.0.225 start simaticarcorwebapi
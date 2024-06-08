# ArCloud_SimaticInterface

Fork de Arcor para el proyecto de Colombia - ARCLOUD.


## A REVIZAR!!!

## Como generar una nueva versión

Suponemos que usted estuvo trabajando siguiendo el modelo propuesto por Gitflow, es decir, que tiene una rama en el repositorio con todos los cambios y quiere crear una nueva versión para subir a QA (y luego a producción).

* Primero hay que finalizar el feature en el cual estuvo trabajando:

	```bash
	git flow feature finish <nombre de su feature o rama>
	```
	
* Luego debe crear un release cuyo nombre sea la versión que quiere que tenga el tag cuando se haga el merge a master. Por ejemplo:

	```bash
	git flow release start 1.8.65
	```
	
* Luego abrir el proyecto en el Visual Studio.
* Click derecho sobre el proyecto SimaticArcorWebApi -> Properties -> Package
* Setear el campo "Package version" con el mismo valor de versión que el nombre del release (para este ejemplo, 1.8.65).
* Guardar y cerrar el Visual Studio
* Commitear el cambio en el git.
* Finalizar el release con:

	```bash
	git flow release finish 1.8.65
	```
	
## Como crear un hotfix

Este caso se da cuando hay que hacer un hotfix sobre la rama master.

* Primero se crea el hotfix, siguiendo el ejemplo anterior (notese que el nombre del hotfix es una versión mayor al release anterior):

	```bash
	git flow hotfix start 1.8.66
	```
	
* Se hacen los cambios, se commitean, etc. 
* Click derecho sobre el proyecto SimaticArcorWebApi -> Properties -> Package
* Setear el campo "Package version" con el mismo valor de versión que el nombre del release (para este ejemplo, 1.8.66).
* Guardar y cerrar el Visual Studio
* Commitear el cambio en el git.
* Se cierra el hotfix con:

	```bash
	git flow hotfix finish 1.8.66
	```
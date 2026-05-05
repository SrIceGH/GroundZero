using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{

    //Estas son las variables principales para interactuar con el playerimput, el rigidbody y el animator.
    public PlayerInput playerInput;
    Rigidbody rb;
    private Animator animator;

    //Aqui empiezan las variables generales de movimiento.
    [SerializeField] float playerSpeed; //Velocidad del jugador
    [SerializeField] float playerAccelerationMov; //A cuanta velocidad llega al movimiento de la playerSpeed.
    [SerializeField] float playerAccelerationLook; //A cuanta velocidad gira hasta llegar a donde tiene que mirar (Es diferente)
    bool playerBlockActive; //Para ver cuando esta bloqueando
    bool playerBoltActive; //Para ver cuando está haciendo un abatimiento.
    [SerializeField] float playerBoltForceAdded; //Cuanta fuerza añadida hay en un abatimiento
    Vector3 playerBoltDir = Vector3.zero; //Hacia donde apunta el abatimiento. Será zero si no hay ninguno actualmente.
    float playerBoltForceReductor; //El abatimiento tiene una fuerza x, y cada frame que pasa vuelve a aplicar esa fuerza, 
    // pero siendo en el siguiente frame x/playerBoltForceReductor, disminuyendo tras cada frame hasta el ultimo.
    [SerializeField] float playerBoltForceLimit; //Los frames en los que esta activo este abatimiemto.


    //Aqui se declaran las variables de la protección y los placeholders para verlo visualmente.
    [SerializeField] GameObject shieldPrefab;
    [SerializeField] GameObject placeholder1;
    [SerializeField] GameObject placeholder2;
    [SerializeField] GameObject placeholder3;
    [SerializeField] GameObject placeholder4;

    void Start()
    {
        //Llamamos a los ensenciales
        rb = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();
        animator = GetComponent<Animator>();

        //Y tambien declaramos la playerBoltForceReductor. Empezará siendo igual a playerBoltForceLimit y al llamarlo será igual a 1
        playerBoltForceReductor = playerBoltForceLimit;
    }

    void FixedUpdate()
    {
        //En PrejectSettings, Time, fixedTimestep lo he cambiado para que sea 60fps en vez de 50fps, ya que es el estandar en juegos del genero.
        
        /*//Aqui miramos para activar o desactivar el escudo.
        if (playerInput.actions["Block"].IsPressed())
        {
            shieldPrefab.SetActive(true);
            playerBlockActive = true;
        }
        else
        {
            shieldPrefab.SetActive(false);
            playerBlockActive = false; //Mensaje de recordatorio por y para Joel: Parry. Si no eres Joel, no le intentes buscar sentido.
            // Y si quieres buscarselo, preguntaselo directamente y no saques suposiciones precipitadas.
        }

        //Aqui vemos si el jugador quiere hacer un abatimiento o no. Revisamos:
        if (playerInput.actions["Bolt"].IsPressed() //Si quiere hacer la acción.
            && playerBoltActive == false //Si no lo está haciendo ya.*
            && playerBoltForceReductor==playerBoltForceLimit) //Y si está listo para hacerlo.*
        {//(*)Se que de primeras estos dos pueden parecer rebundantes, pero son necesarios.
         //Sin ellos, no se reiniciaría ni el vector de movimiento ni el reductor.

            playerBoltActive = true; //Decimos "Oye, ya lo está haciendo"
            playerBoltForceReductor=1; //Y preparamos la variable para que empiece a hacerlo.
        }
        else //Mientras esté activo, es imposible que se ejecute lo de arriba, conque siempre hará lo de aqui abajo mientras este abatiendo.
        {
            if (playerBoltForceReductor<playerBoltForceLimit) //Mientras no haya llegado, irá sumando, lo que significa que irá reduciendo el empuje.
            {
                playerBoltForceReductor+=1;
            }
            else //Si ya ha llegado o superado al limite:
            {
                playerBoltActive = false; //Marcamos que ya no lo hace
                playerBoltForceReductor = playerBoltForceLimit; //Igualamos por si se ha pasado
                playerBoltDir = Vector3.zero; //Y volvemos a marcarlo como 0 para que no de problemas.
            }
        }

        //Estos debuglogs son para ver que funciona bien. Son Dispensables.
        //Por poder, puedes retirarlos cuando los veas necesarios, aunque creo buena idea mantenerlos un poco mas.
        Debug.Log(playerBoltForceReductor);
        Debug.Log(playerBoltForceLimit);
        Debug.Log(playerBoltActive == false);
        Debug.Log(playerInput.actions["Bolt"].IsPressed() && playerBoltActive == false && playerBoltForceReductor==playerBoltForceLimit);
        */
        //Quiero hacer un movimiento suave y no-instantaneo, pero rapido y responsivo, conque...
        //Cojo el imput del jugador (El "Hacia adelante" del personaje es Z+, cabe recalcar)
        Debug.Log("Esta vaina funciona");
        Vector2 playerInputMov = playerInput.actions["Mov"].ReadValue<Vector2>();

        //Y lo transformo en un vector de movimiento.
        Vector3 movementIntendedMov = new Vector3(playerInputMov.x, 0, -playerInputMov.y) * playerSpeed; //(influido por la speed)

        //Cojo el actual movimiento que tiene el jugador,
        Vector3 movementActualMov = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y, rb.linearVelocity.z);

        //Y calculo el vector resultante como un producto vectorial, haciendolo gradualmente gracias a la acceleracion como "t" cada frame para hacerlo suavemente (Olé)
        Vector3 movementGoingToMov = Vector3.Lerp(movementActualMov, movementIntendedMov, playerAccelerationMov * Time.fixedDeltaTime);
        
        //Puede ser algo complicado entenderlo así, conque te lo explico brevemente aqui:
        //Imaginate que tienes 2 vacas:
        // -La primera mira hacia donde quiere, de forma instantanea
        // -La segunda intenta imitarla lo mejor que puede

        // Visto de forma visual, sería así: (Las bacas son "(OccO)")

        // (1º)  /\                 | (2º)  /\            _.   | (3º)  /\         .;      | (4º)  /\        /\       |
        //       ||                 |       ||           .´'   |       ||         /       |       ||        ||       |
        //     (OccO)    (OccO) =>  |     (OccO)    (OccO)     |     (OccO)    (OccO)     |     (OccO)    (OccO)     |
        //                          |                          |                          |                          |
        //                          |                          |                          |                          |

        //En sí, la segunda vaca es mas suave, mas "Humana", no teletransporta su mirada,
        //sino que se desplaza hacia donde quiere mirar de forma gradual.
        //Primero lo hace rapido, y por el paso del tiempo se va acercando mas y mas.
        //Tal y como lo hace una asindota:

        // | 
        // |  (OccO) \                                                                                                 |
        // |          \                               ejemplo: f(x) = 1 + ( 1/(x-1) )                                  |
        // |           `.                                                                                              |
        // |             ¨`·_            Si ya lo ves, tremendo! Pero si no,                                           |
        // |                 ¨`-·._                    entra al GraphToy que nos enseñó el de VFX                      |
        // |                       ¨``--··..___                                        en clase si lo ves optimo.      |
        // |                                   ¨¨¨¨``----····........._____________                                    |
        // |  (OccO) ..............................................................iiiiiiiiiiiiiiiiiiii::::::::::::::::|
        // |                                                                                                           |

        //No mucho mas. Preguntame si lo necesitas! Y si no lo necesitas, mucho mejor, menos lio!

        //Este debuglog está para ver como se está moviendose el jugador. Lo mismo que con los otros debuglogs.
        Debug.Log(movementIntendedMov);

        //Aqui es cuando entra el abatimiento.
        if (playerBoltActive == true) //Si está en proceso de bolt:
        {
            if (playerBoltDir == Vector3.zero) //calculo el vector si no lo ha hecho todavia.
            {
                playerBoltDir = movementIntendedMov; //Y lo igualo hacia donde quiere hacerlo.
            }
            //Y modifico el movimiento que va a hacer (El de la segunda vaca) para que abata hacia esa dirección.
            movementGoingToMov = new Vector3 
            (
                movementGoingToMov.x + (playerBoltDir.x * (playerBoltForceAdded / playerBoltForceReductor)),
                movementGoingToMov.y, //Y en la linea de arriba y abajo de esta misma puedes ver la logica detras del "empuje".
                movementGoingToMov.z + (playerBoltDir.z * (playerBoltForceAdded / playerBoltForceReductor))
            );
        }

        //Y, al final, muevo al propio jugador con ese movimiento, esté abatiendo o no.
        rb.linearVelocity = new Vector3(movementGoingToMov.x, rb.linearVelocity.y, movementGoingToMov.z); //(Teniendo en cuenta la gravedad y el momento vertical)

        //Así, cada vez que se mueva, se irá hacercando lo que quiere el jugador siendo calculado, pero no tesco o inmediato.
        
        //Tras eso, lo rotamos hacia donde el jugador quiere mirar al estar moviendose.
        Vector3 movementGoingToLook = new Vector3(playerInputMov.x, 0, -playerInputMov.y);
        if (movementGoingToLook.sqrMagnitude > 0.01f) //(Si se esta moviendo)
        {   
            //Para entender este movimiento, imaginatelo como una tercera vaca con mucho mejores reflejos que la segunda.
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.LookRotation(movementGoingToLook), playerAccelerationLook * Time.fixedDeltaTime));
        }

        //Y ya estaría. Lo siguiente intenta no tocarlo. No molesta ni impide, solo son placeholders que necesito.

        if (playerInput.actions["AttackSlot1"].IsPressed()) //XXXXXXXXXXX(NO TOCAR, PLACEHOLDER)XXXXXXXXXXX
        {
            placeholder1.SetActive(true);
        }
        else
        {
            placeholder1.SetActive(false);
        } 
        if (playerInput.actions["AttackSlot2"].IsPressed()) //XXXXXXXXXXX(NO TOCAR, PLACEHOLDER)XXXXXXXXXXX
        {
            placeholder2.SetActive(true);
        }
        else
        {
            placeholder2.SetActive(false);
        } 
        if (playerInput.actions["AttackSlot3"].IsPressed()) //XXXXXXXXXXX(NO TOCAR, PLACEHOLDER)XXXXXXXXXXX
        {
            placeholder3.SetActive(true);
        }
        else
        {
            placeholder3.SetActive(false);
        } 
        if (playerInput.actions["AttackSlot4"].IsPressed()) //XXXXXXXXXXX(NO TOCAR, PLACEHOLDER)XXXXXXXXXXX
        {
            placeholder4.SetActive(true);
        }
        else
        {
            placeholder4.SetActive(false);
        } 
    }

}

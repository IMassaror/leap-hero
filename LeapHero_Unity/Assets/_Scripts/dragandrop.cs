using UnityEngine;

public class DragAndDrop2D : MonoBehaviour
{
    private Vector3 offset;
    private float zCoordinate;

    // Chamado quando o botão do mouse é pressionado sobre o colisor
    void OnMouseDown()
    {
        // Armazena a coordenada Z original do objeto
        zCoordinate = Camera.main.WorldToScreenPoint(gameObject.transform.position).z;

        // Calcula o offset (deslocamento) inicial entre o centro do objeto e o ponteiro do mouse
        offset = gameObject.transform.position - GetMouseWorldPos();
    }

    // Chamado a cada frame enquanto o botão do mouse está pressionado sobre o colisor
    void OnMouseDrag()
    {
        // Move o objeto para a posição atual do mouse, mantendo o offset original
        transform.position = GetMouseWorldPos() + offset;
    }

    // Função auxiliar para converter a posição do mouse (pixel) para a posição do mundo (Unity world space)
    private Vector3 GetMouseWorldPos()
    {
        // Posição do mouse na tela (em pixels)
        Vector3 mousePoint = Input.mousePosition;
        
        // Define a coordenada Z para a distância da câmera correta
        mousePoint.z = zCoordinate;
        
        // Converte a posição da tela para a posição do mundo
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }
}
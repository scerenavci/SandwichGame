using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SandwichElement : MonoBehaviour
{
   RigidbodyConstraints originalConstraints;

   private void Awake()
   {
      originalConstraints = gameObject.GetComponent<Rigidbody>().constraints;
   }

   private void OnCollisionStay(Collision other)
   {
      if(other.gameObject.CompareTag("Plane"))
         return;

      FreezeAll();
      
      if (gameObject.GetComponent<FixedJoint>() ==null)
      {
         gameObject.AddComponent<FixedJoint>();
         
      }
      var fixedJoint = gameObject.GetComponent<FixedJoint>();
      fixedJoint.enableCollision= true;
      fixedJoint.connectedBody = other.rigidbody;
   
   }

   private void OnCollisionExit(Collision other)
   {
      SetOriginalConstrains();
      Destroy(gameObject.GetComponent<FixedJoint>());
   }

   public void FreezeAll()
   {
      gameObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
   }
   public void SetOriginalConstrains()
   {
      gameObject.GetComponent<Rigidbody>().constraints = originalConstraints;
   }
}

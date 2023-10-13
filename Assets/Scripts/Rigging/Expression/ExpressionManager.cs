using UnityEngine;

public class ExpressionManager : MonoBehaviour
{
    public enum Vowel { A = 0, E, I, O, U}

    public Animator faceAnim;

  

    public void SetVowel(int vowelIndex)
    {
        SetVowel((Vowel)vowelIndex);
    }

    public void SetVowel(Vowel vowel)
    {
        switch (vowel)
        {
            case Vowel.A:
                faceAnim.SetTrigger("A");
                break;
            case Vowel.E:
                faceAnim.SetTrigger("E");
                break;
            case Vowel.I:
                faceAnim.SetTrigger("I");
                break;
            case Vowel.O:
                faceAnim.SetTrigger("O");
                break;
            case Vowel.U:
                faceAnim.SetTrigger("U");
                break;
        }
    }

    public void SetTrigger(string trigName)
    {
        faceAnim.SetTrigger(trigName);
    }

    public void SetBoolTrue(string boolName)
    {
        faceAnim.SetBool(boolName, true);
    }

    public void SetBoolFalse(string boolName)
    {
        faceAnim.SetBool(boolName, false);
    }


}

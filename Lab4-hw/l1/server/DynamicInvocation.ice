module DynamicInvocation
{
    struct NonTrivialStruct
    {
        int id;
        string name;
    };
    
    sequence<NonTrivialStruct> NonTrivialStructSeq;

    
    interface MoreTrivialService
    {
        int add(int a, int b);
        string concat(string a, string b);
        int processList(NonTrivialStructSeq list);
    };  
};
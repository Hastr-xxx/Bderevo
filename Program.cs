using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

public class BTreeNode
{
    public int[] Keys;          
    public int Degree;          
    public BTreeNode[] Children; 
    public int KeyCount;        
    public bool IsLeaf;         

    public BTreeNode(int degree, bool isLeaf)
    {
        Degree = degree;
        IsLeaf = isLeaf;
        Keys = new int[2 * degree - 1];   
        Children = new BTreeNode[2 * degree]; 
        KeyCount = 0;
    }
}

public class BTree
{
    private BTreeNode root;
    private int degree; 
    public int OperationCount { get; private set; } 

    public BTree(int degree)
    {
        this.degree = degree;
        root = new BTreeNode(degree, true);
    }

    public void ResetOperationCount()
    {
        OperationCount = 0;
    }

    public bool Search(int key)
    {
        ResetOperationCount();
        return SearchInternal(root, key) != null;
    }

    private BTreeNode SearchInternal(BTreeNode node, int key)
    {
        if (node == null) return null;
        int i = 0;
        while (i < node.KeyCount && key > node.Keys[i])
        {
            OperationCount++;
            i++;
        }
        OperationCount++;

        if (i < node.KeyCount && key == node.Keys[i])
        {
            return node;
        }

        if (node.IsLeaf)
            return null;

        return SearchInternal(node.Children[i], key);
    }

    public void Insert(int key)
    {
        ResetOperationCount();
        BTreeNode r = root;

        if (r.KeyCount == 2 * degree - 1)
        {
            OperationCount++;
            BTreeNode s = new BTreeNode(degree, false);
            root = s;
            s.Children[0] = r;
            SplitChild(s, 0, r);
            InsertNonFull(s, key);
        }
        else
        {
            InsertNonFull(r, key);
        }
    }

    private void InsertNonFull(BTreeNode node, int key)
    {
        int i = node.KeyCount - 1;

        if (node.IsLeaf)
        {
            while (i >= 0 && key < node.Keys[i])
            {
                OperationCount++;
                node.Keys[i + 1] = node.Keys[i];
                i--;
            }
            OperationCount++;
            node.Keys[i + 1] = key;
            node.KeyCount++;
        }
        else
        {
            while (i >= 0 && key < node.Keys[i])
            {
                OperationCount++;
                i--;
            }
            OperationCount++;
            i++;

            if (node.Children[i].KeyCount == 2 * degree - 1)
            {
                OperationCount++;
                SplitChild(node, i, node.Children[i]);
                if (key > node.Keys[i])
                {
                    OperationCount++;
                    i++;
                }
            }
            InsertNonFull(node.Children[i], key);
        }
    }

    private void SplitChild(BTreeNode parentNode, int childIndex, BTreeNode childToSplit)
    {
        BTreeNode newNode = new BTreeNode(childToSplit.Degree, childToSplit.IsLeaf);
        newNode.KeyCount = degree - 1;

        for (int j = 0; j < degree - 1; j++)
        {
            newNode.Keys[j] = childToSplit.Keys[j + degree];
        }

        if (!childToSplit.IsLeaf)
        {
            for (int j = 0; j < degree; j++)
            {
                newNode.Children[j] = childToSplit.Children[j + degree];
            }
        }

        childToSplit.KeyCount = degree - 1;

        for (int j = parentNode.KeyCount; j >= childIndex + 1; j--)
        {
            parentNode.Children[j + 1] = parentNode.Children[j];
        }
        parentNode.Children[childIndex + 1] = newNode;

        for (int j = parentNode.KeyCount - 1; j >= childIndex; j--)
        {
            parentNode.Keys[j + 1] = parentNode.Keys[j];
        }
        parentNode.Keys[childIndex] = childToSplit.Keys[degree - 1];
        parentNode.KeyCount++;
    }

    public void Delete(int key)
    {
        ResetOperationCount();
        if (root == null)
        {
            Console.WriteLine("Дерево пустое");
            return;
        }
        DeleteInternal(root, key);

        if (root.KeyCount == 0)
        {
            if (root.IsLeaf)
                root = null;
            else
                root = root.Children[0];
        }
    }

    private void DeleteInternal(BTreeNode node, int key)
    {
        int idx = FindKeyIndex(node, key);

        if (idx < node.KeyCount && node.Keys[idx] == key)
        {
            OperationCount++;
            if (node.IsLeaf)
                RemoveFromLeaf(node, idx);
            else
                RemoveFromNonLeaf(node, idx);
        }
        else
        {
            if (node.IsLeaf)
            {
                Console.WriteLine($"Ключ {key} не найден в дереве.");
                return;
            }

            bool isLastChild = (idx == node.KeyCount);
            OperationCount++;

            if (node.Children[idx].KeyCount == degree - 1)
            {
                OperationCount++;
                FillChild(node, idx);
            }

            if (isLastChild && idx > node.KeyCount)
                DeleteInternal(node.Children[idx - 1], key);
            else
                DeleteInternal(node.Children[idx], key);
        }
    }

    private int FindKeyIndex(BTreeNode node, int key)
    {
        int idx = 0;
        while (idx < node.KeyCount && key > node.Keys[idx])
        {
            OperationCount++;
            idx++;
        }
        OperationCount++;
        return idx;
    }

    private void RemoveFromLeaf(BTreeNode node, int idx)
    {
        for (int i = idx + 1; i < node.KeyCount; ++i)
            node.Keys[i - 1] = node.Keys[i];
        node.KeyCount--;
    }

    private void RemoveFromNonLeaf(BTreeNode node, int idx)
    {
        int key = node.Keys[idx];

        if (node.Children[idx].KeyCount >= degree)
        {
            OperationCount++;
            int pred = GetPredecessor(node, idx);
            node.Keys[idx] = pred;
            DeleteInternal(node.Children[idx], pred);
        }
        else if (node.Children[idx + 1].KeyCount >= degree)
        {
            OperationCount++;
            int succ = GetSuccessor(node, idx);
            node.Keys[idx] = succ;
            DeleteInternal(node.Children[idx + 1], succ);
        }
        else
        {
            MergeChildren(node, idx);
            DeleteInternal(node.Children[idx], key);
        }
    }

    private int GetPredecessor(BTreeNode node, int idx)
    {
        BTreeNode cur = node.Children[idx];
        while (!cur.IsLeaf)
            cur = cur.Children[cur.KeyCount];
        return cur.Keys[cur.KeyCount - 1];
    }

    private int GetSuccessor(BTreeNode node, int idx)
    {
        BTreeNode cur = node.Children[idx + 1];
        while (!cur.IsLeaf)
            cur = cur.Children[0];
        return cur.Keys[0];
    }

    private void FillChild(BTreeNode node, int idx)
    {
        if (idx != 0 && node.Children[idx - 1].KeyCount >= degree)
        {
            OperationCount++;
            BorrowFromPrev(node, idx);
        }
        else if (idx != node.KeyCount && node.Children[idx + 1].KeyCount >= degree)
        {
            OperationCount++;
            BorrowFromNext(node, idx);
        }
        else
        {
            if (idx != node.KeyCount)
                MergeChildren(node, idx);
            else
                MergeChildren(node, idx - 1);
        }
    }

    private void BorrowFromPrev(BTreeNode node, int idx)
    {
        BTreeNode child = node.Children[idx];
        BTreeNode sibling = node.Children[idx - 1];

        for (int i = child.KeyCount - 1; i >= 0; --i)
            child.Keys[i + 1] = child.Keys[i];

        if (!child.IsLeaf)
        {
            for (int i = child.KeyCount; i >= 0; --i)
                child.Children[i + 1] = child.Children[i];
        }

        child.Keys[0] = node.Keys[idx - 1];
        if (!child.IsLeaf)
            child.Children[0] = sibling.Children[sibling.KeyCount];

        node.Keys[idx - 1] = sibling.Keys[sibling.KeyCount - 1];
        child.KeyCount++;
        sibling.KeyCount--;
    }

    private void BorrowFromNext(BTreeNode node, int idx)
    {
        BTreeNode child = node.Children[idx];
        BTreeNode sibling = node.Children[idx + 1];

        child.Keys[child.KeyCount] = node.Keys[idx];
        if (!child.IsLeaf)
            child.Children[child.KeyCount + 1] = sibling.Children[0];

        node.Keys[idx] = sibling.Keys[0];
        for (int i = 1; i < sibling.KeyCount; ++i)
            sibling.Keys[i - 1] = sibling.Keys[i];

        if (!sibling.IsLeaf)
        {
            for (int i = 1; i <= sibling.KeyCount; ++i)
                sibling.Children[i - 1] = sibling.Children[i];
        }

        child.KeyCount++;
        sibling.KeyCount--;
    }

    private void MergeChildren(BTreeNode node, int idx)
    {
        BTreeNode leftChild = node.Children[idx];
        BTreeNode rightChild = node.Children[idx + 1];

        leftChild.Keys[degree - 1] = node.Keys[idx];

        for (int i = 0; i < rightChild.KeyCount; ++i)
            leftChild.Keys[i + degree] = rightChild.Keys[i];

        if (!leftChild.IsLeaf)
        {
            for (int i = 0; i <= rightChild.KeyCount; ++i)
                leftChild.Children[i + degree] = rightChild.Children[i];
        }

        for (int i = idx + 1; i < node.KeyCount; ++i)
            node.Keys[i - 1] = node.Keys[i];

        for (int i = idx + 2; i <= node.KeyCount; ++i)
            node.Children[i - 1] = node.Children[i];

        leftChild.KeyCount += rightChild.KeyCount + 1;
        node.KeyCount--;
        rightChild = null;
    }
}

class Program
{
    class MeasurementResult
    {
        public int Index { get; set; }
        public double TimeMilliseconds { get; set; }
        public int OperationCount { get; set; }
    }

    static void Main(string[] args)
    {

        BTree bTree = new BTree(3);

        int arraySize = 10000;
        int[] randomNumbers = new int[arraySize];
        Random rand = new Random();
        for (int i = 0; i < arraySize; i++)
        {
            randomNumbers[i] = rand.Next(1, 20001);
        }

        List<MeasurementResult> insertResults = new List<MeasurementResult>();
        List<MeasurementResult> searchResults = new List<MeasurementResult>();
        List<MeasurementResult> deleteResults = new List<MeasurementResult>();

        Stopwatch sw = new Stopwatch();

        for (int i = 0; i < randomNumbers.Length; i++)
        {
            sw.Restart();
            bTree.Insert(randomNumbers[i]);
            sw.Stop();

            insertResults.Add(new MeasurementResult
            {
                Index = i,
                TimeMilliseconds = sw.Elapsed.TotalMilliseconds,
                OperationCount = bTree.OperationCount
            });
        }
        var searchIndices = Enumerable.Range(0, arraySize).OrderBy(x => rand.Next()).Take(100).ToList();
        foreach (int idx in searchIndices)
        {
            int keyToFind = randomNumbers[idx];
            sw.Restart();
            bool found = bTree.Search(keyToFind);
            sw.Stop();
            searchResults.Add(new MeasurementResult
            {
                Index = idx,
                TimeMilliseconds = sw.Elapsed.TotalMilliseconds,
                OperationCount = bTree.OperationCount
            });
        }

        var deleteIndices = Enumerable.Range(0, arraySize).OrderBy(x => rand.Next()).Take(1000).ToList();
        foreach (int idx in deleteIndices)
        {
            int keyToDelete = randomNumbers[idx];
            sw.Restart();
            bTree.Delete(keyToDelete);
            sw.Stop();
            deleteResults.Add(new MeasurementResult
            {
                Index = idx,
                TimeMilliseconds = sw.Elapsed.TotalMilliseconds,
                OperationCount = bTree.OperationCount
            });
        }

        Console.WriteLine($"Среднее время добавления (мс): {insertResults.Average(r => r.TimeMilliseconds):F4}");
        Console.WriteLine($"Среднее время поиска (мс): {searchResults.Average(r => r.TimeMilliseconds):F4}");
        Console.WriteLine($"Среднее время удаления (мс): {deleteResults.Average(r => r.TimeMilliseconds):F4}");
        Console.WriteLine($"Среднее кол-во операций добавления: {insertResults.Average(r => r.OperationCount):F2}");
        Console.WriteLine($"Среднее кол-во операций поиска: {searchResults.Average(r => r.OperationCount):F2}");
        Console.WriteLine($"Среднее кол-во операций удаления: {deleteResults.Average(r => r.OperationCount):F2}");

        Console.ReadKey();
    }

    static void SaveResultsToCsv(List<MeasurementResult> results, string fileName, string header)
    {
        try
        {
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                sw.WriteLine(header);
                foreach (var res in results)
                {
                    sw.WriteLine($"{res.Index},{res.TimeMilliseconds:F4},{res.OperationCount}");
                }
            }
        }
        catch (Exception ex) { }
    }
}
```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean         | Error      | StdDev    | Gen0   | Allocated |
|----------------- |-------------:|-----------:|----------:|-------:|----------:|
| ComparePositions |     3.468 ns |  0.0262 ns | 0.0014 ns |      - |         - |
| FromBytes        | 2,007.237 ns | 69.2197 ns | 3.7942 ns | 0.0420 |     744 B |
| ToBytes          |   939.382 ns | 74.9621 ns | 4.1089 ns | 0.0153 |     264 B |

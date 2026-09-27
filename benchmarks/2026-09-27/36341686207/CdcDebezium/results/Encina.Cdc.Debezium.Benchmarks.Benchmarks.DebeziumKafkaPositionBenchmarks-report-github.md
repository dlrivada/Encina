```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 4.13GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean         | Error      | StdDev     | Gen0   | Allocated |
|----------------- |-------------:|-----------:|-----------:|-------:|----------:|
| ComparePositions |     2.361 ns |   1.411 ns |  0.0774 ns |      - |         - |
| FromBytes        | 1,447.396 ns | 985.985 ns | 54.0452 ns | 0.0076 |     744 B |
| ToBytes          |   554.675 ns |  13.906 ns |  0.7622 ns | 0.0029 |     264 B |

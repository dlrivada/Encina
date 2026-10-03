```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean         | Error         | StdDev     | Gen0   | Allocated |
|----------------- |-------------:|--------------:|-----------:|-------:|----------:|
| ComparePositions |     2.418 ns |     0.4105 ns |  0.0225 ns |      - |         - |
| FromBytes        | 1,519.536 ns | 1,160.3149 ns | 63.6008 ns | 0.0076 |     744 B |
| ToBytes          |   556.698 ns |    66.8803 ns |  3.6659 ns | 0.0029 |     264 B |

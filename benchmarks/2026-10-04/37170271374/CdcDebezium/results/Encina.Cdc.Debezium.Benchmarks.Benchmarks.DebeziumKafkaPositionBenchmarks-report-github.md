```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean         | Error     | StdDev     | Gen0   | Allocated |
|----------------- |-------------:|----------:|-----------:|-------:|----------:|
| ComparePositions |     3.477 ns | 0.0107 ns |  0.0154 ns |      - |         - |
| FromBytes        | 2,005.426 ns | 9.2990 ns | 13.3363 ns | 0.0420 |     744 B |
| ToBytes          |   954.322 ns | 1.9247 ns |  2.7603 ns | 0.0153 |     264 B |

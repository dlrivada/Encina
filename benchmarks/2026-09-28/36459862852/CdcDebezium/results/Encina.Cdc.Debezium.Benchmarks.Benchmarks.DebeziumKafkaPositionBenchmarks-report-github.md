```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean         | Error     | StdDev    | Gen0   | Allocated |
|----------------- |-------------:|----------:|----------:|-------:|----------:|
| ComparePositions |     3.504 ns |  1.161 ns | 0.0637 ns |      - |         - |
| FromBytes        | 2,025.544 ns | 65.668 ns | 3.5995 ns | 0.0420 |     744 B |
| ToBytes          |   949.918 ns | 11.968 ns | 0.6560 ns | 0.0153 |     264 B |

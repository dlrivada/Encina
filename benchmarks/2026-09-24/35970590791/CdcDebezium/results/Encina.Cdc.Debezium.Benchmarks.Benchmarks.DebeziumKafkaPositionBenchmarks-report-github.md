```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean         | Error       | StdDev    | Gen0   | Allocated |
|----------------- |-------------:|------------:|----------:|-------:|----------:|
| ComparePositions |     2.688 ns |   0.2490 ns | 0.0136 ns |      - |         - |
| FromBytes        | 1,502.463 ns | 139.6712 ns | 7.6559 ns | 0.0439 |     744 B |
| ToBytes          |   667.150 ns |  11.4380 ns | 0.6270 ns | 0.0153 |     264 B |

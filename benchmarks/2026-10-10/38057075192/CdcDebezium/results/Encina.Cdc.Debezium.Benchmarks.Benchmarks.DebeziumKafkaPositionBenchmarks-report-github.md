```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean         | Error     | StdDev    | Gen0   | Allocated |
|----------------- |-------------:|----------:|----------:|-------:|----------:|
| ComparePositions |     1.633 ns |  1.774 ns | 0.0972 ns |      - |         - |
| FromBytes        | 1,103.566 ns | 54.139 ns | 2.9675 ns | 0.0439 |     744 B |
| ToBytes          |   469.321 ns | 34.206 ns | 1.8749 ns | 0.0153 |     264 B |

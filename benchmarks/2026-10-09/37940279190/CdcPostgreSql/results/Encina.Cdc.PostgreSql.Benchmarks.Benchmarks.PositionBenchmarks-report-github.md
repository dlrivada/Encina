```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.4456 ns | 0.1660 ns | 0.0091 ns |  0.12 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 3.6803 ns | 5.6951 ns | 0.3122 ns |  1.00 |    0.10 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.5132 ns | 0.9768 ns | 0.0535 ns |  0.96 |    0.07 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.9624 ns | 3.0543 ns | 0.1674 ns |  1.08 |    0.09 | 0.0019 |      32 B |        1.33 |

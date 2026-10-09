```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.16GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.4781 ns | 0.0570 ns | 0.0031 ns |  0.15 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 3.2639 ns | 1.2511 ns | 0.0686 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.5332 ns | 0.2610 ns | 0.0143 ns |  1.08 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.3774 ns | 0.3775 ns | 0.0207 ns |  1.04 |    0.02 | 0.0019 |      32 B |        1.33 |

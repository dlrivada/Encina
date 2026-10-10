```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.4631 ns | 0.5387 ns | 0.0295 ns |  0.14 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 3.3435 ns | 2.8250 ns | 0.1548 ns |  1.00 |    0.06 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.6643 ns | 3.5637 ns | 0.1953 ns |  1.10 |    0.07 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.8905 ns | 0.4106 ns | 0.0225 ns |  1.17 |    0.05 | 0.0019 |      32 B |        1.33 |

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 131.98 ns |  5.849 ns | 0.321 ns |  7.43 |    0.11 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  17.77 ns |  5.279 ns | 0.289 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 598.16 ns | 70.300 ns | 3.853 ns | 33.67 |    0.51 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 576.65 ns | 18.942 ns | 1.038 ns | 32.46 |    0.46 | 0.0601 |    1008 B |       42.00 |

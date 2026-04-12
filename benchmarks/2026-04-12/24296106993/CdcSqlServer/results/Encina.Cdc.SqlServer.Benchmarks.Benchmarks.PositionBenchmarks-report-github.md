```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.8682 ns | 0.0013 ns | 0.0018 ns |  0.17 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 5.2770 ns | 0.2737 ns | 0.4096 ns |  1.01 |    0.11 | 0.0010 |      24 B |        1.00 |
| FromBytes        | 6.2383 ns | 0.3519 ns | 0.5267 ns |  1.19 |    0.14 | 0.0010 |      24 B |        1.00 |
| ToBytes          | 6.3908 ns | 0.2583 ns | 0.3866 ns |  1.22 |    0.12 | 0.0013 |      32 B |        1.33 |

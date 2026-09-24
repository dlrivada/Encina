```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 134.63 ns | 15.113 ns | 0.828 ns | 11.33 |    0.29 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  11.89 ns |  6.338 ns | 0.347 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 597.13 ns | 39.288 ns | 2.154 ns | 50.25 |    1.28 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 598.83 ns | 30.508 ns | 1.672 ns | 50.39 |    1.28 | 0.0601 |    1008 B |       42.00 |

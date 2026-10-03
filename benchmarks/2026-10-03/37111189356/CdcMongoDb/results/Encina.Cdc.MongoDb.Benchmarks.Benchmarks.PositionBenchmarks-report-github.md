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
| ComparePositions | 134.35 ns | 16.252 ns | 0.891 ns | 11.26 |    0.25 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  11.93 ns |  5.296 ns | 0.290 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 610.14 ns | 62.467 ns | 3.424 ns | 51.16 |    1.12 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 602.68 ns | 23.501 ns | 1.288 ns | 50.53 |    1.08 | 0.0601 |    1008 B |       42.00 |

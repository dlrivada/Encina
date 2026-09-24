```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.72GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 131.30 ns |  10.67 ns | 0.585 ns | 10.23 |    1.40 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  13.04 ns |  37.00 ns | 2.028 ns |  1.02 |    0.20 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 629.98 ns | 169.59 ns | 9.296 ns | 49.10 |    6.74 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 628.47 ns |  69.38 ns | 3.803 ns | 48.98 |    6.70 | 0.0601 |    1008 B |       42.00 |

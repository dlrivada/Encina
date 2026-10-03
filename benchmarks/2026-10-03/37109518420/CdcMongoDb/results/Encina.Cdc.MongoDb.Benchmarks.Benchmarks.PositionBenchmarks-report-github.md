```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.06GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error      | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|-----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 133.89 ns |   7.375 ns | 0.404 ns |  9.70 |    0.73 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  13.87 ns |  22.058 ns | 1.209 ns |  1.01 |    0.11 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 616.19 ns | 106.890 ns | 5.859 ns | 44.66 |    3.36 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 596.61 ns |  80.793 ns | 4.429 ns | 43.24 |    3.25 | 0.0601 |    1008 B |       42.00 |

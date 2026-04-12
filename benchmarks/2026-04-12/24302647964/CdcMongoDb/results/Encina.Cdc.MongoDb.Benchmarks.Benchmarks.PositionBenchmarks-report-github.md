```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 136.73 ns | 0.506 ns | 0.725 ns | 10.12 |    1.12 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  13.67 ns | 0.947 ns | 1.417 ns |  1.01 |    0.15 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 600.77 ns | 3.239 ns | 4.645 ns | 44.45 |    4.91 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 565.71 ns | 3.229 ns | 4.631 ns | 41.86 |    4.62 | 0.0601 |    1008 B |       42.00 |

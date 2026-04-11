```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 140.85 ns |  0.527 ns |  0.756 ns | 10.62 |    1.33 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  13.47 ns |  1.121 ns |  1.678 ns |  1.02 |    0.18 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 651.36 ns | 14.120 ns | 21.134 ns | 49.12 |    6.35 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 598.35 ns |  4.581 ns |  6.856 ns | 45.12 |    5.68 | 0.0601 |    1008 B |       42.00 |

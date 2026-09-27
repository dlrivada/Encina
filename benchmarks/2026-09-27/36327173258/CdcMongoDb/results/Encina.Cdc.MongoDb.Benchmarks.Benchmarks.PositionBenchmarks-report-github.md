```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 133.23 ns | 34.874 ns | 1.912 ns | 10.43 |    0.41 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  12.79 ns |  9.885 ns | 0.542 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 602.40 ns |  3.248 ns | 0.178 ns | 47.17 |    1.77 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 592.53 ns | 45.491 ns | 2.493 ns | 46.40 |    1.75 | 0.0601 |    1008 B |       42.00 |

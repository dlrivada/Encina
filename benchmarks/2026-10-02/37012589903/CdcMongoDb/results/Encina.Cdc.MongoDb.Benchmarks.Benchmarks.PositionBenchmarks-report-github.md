```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 136.43 ns | 22.60 ns | 1.239 ns |  6.76 |    0.22 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  20.19 ns | 13.00 ns | 0.713 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 652.64 ns | 35.06 ns | 1.922 ns | 32.35 |    1.01 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 618.82 ns | 25.60 ns | 1.403 ns | 30.67 |    0.95 | 0.0601 |    1008 B |       42.00 |

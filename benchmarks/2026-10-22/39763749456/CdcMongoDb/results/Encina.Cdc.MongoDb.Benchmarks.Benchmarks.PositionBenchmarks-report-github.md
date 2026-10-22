```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 137.52 ns |  5.936 ns | 0.325 ns | 13.55 |    0.39 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  10.16 ns |  5.969 ns | 0.327 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 673.48 ns | 39.495 ns | 2.165 ns | 66.37 |    1.89 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 614.63 ns | 40.144 ns | 2.200 ns | 60.57 |    1.73 | 0.0601 |    1008 B |       42.00 |

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error      | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|-----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 132.40 ns |   4.948 ns | 0.271 ns |  8.98 |    1.58 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  15.19 ns |  61.096 ns | 3.349 ns |  1.03 |    0.27 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 616.23 ns | 103.064 ns | 5.649 ns | 41.81 |    7.37 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 555.84 ns |  31.018 ns | 1.700 ns | 37.71 |    6.64 | 0.0601 |    1008 B |       42.00 |

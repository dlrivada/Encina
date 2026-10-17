```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 103.523 ns | 50.094 ns | 2.7458 ns | 16.01 |    0.40 | 0.0011 |      96 B |        4.00 |
| CreatePosition   |   6.467 ns |  1.344 ns | 0.0737 ns |  1.00 |    0.01 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 453.885 ns | 44.216 ns | 2.4236 ns | 70.19 |    0.76 | 0.0119 |    1024 B |       42.67 |
| ToBytes          | 403.903 ns | 83.107 ns | 4.5554 ns | 62.46 |    0.86 | 0.0119 |    1008 B |       42.00 |

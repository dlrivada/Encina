```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 141.072 ns | 28.741 ns | 1.5754 ns | 17.25 |    0.45 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.184 ns |  4.168 ns | 0.2285 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 589.658 ns | 56.800 ns | 3.1134 ns | 72.09 |    1.79 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 552.910 ns | 34.448 ns | 1.8882 ns | 67.60 |    1.67 | 0.0601 |    1008 B |       42.00 |

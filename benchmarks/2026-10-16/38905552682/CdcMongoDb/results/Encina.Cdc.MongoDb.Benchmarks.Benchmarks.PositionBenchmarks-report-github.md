```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 109.568 ns |   2.635 ns | 0.1444 ns | 17.26 |    0.37 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   6.350 ns |   2.789 ns | 0.1529 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 483.337 ns | 119.505 ns | 6.5505 ns | 76.15 |    1.84 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 443.175 ns |  52.202 ns | 2.8613 ns | 69.82 |    1.53 | 0.0601 |    1008 B |       42.00 |

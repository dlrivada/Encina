```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 110.808 ns |  28.944 ns | 1.5865 ns | 16.52 |    0.50 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   6.714 ns |   3.952 ns | 0.2166 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 489.705 ns |  54.938 ns | 3.0113 ns | 72.99 |    2.05 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 464.729 ns | 109.587 ns | 6.0068 ns | 69.27 |    2.06 | 0.0601 |    1008 B |       42.00 |

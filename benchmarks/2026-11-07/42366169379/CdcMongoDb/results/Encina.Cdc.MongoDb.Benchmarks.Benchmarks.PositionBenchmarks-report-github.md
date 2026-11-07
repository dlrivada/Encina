```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 108.138 ns |   7.021 ns | 0.3848 ns | 16.49 |    0.22 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   6.558 ns |   1.742 ns | 0.0955 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 491.569 ns |  30.098 ns | 1.6498 ns | 74.96 |    0.98 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 453.969 ns | 155.837 ns | 8.5419 ns | 69.23 |    1.43 | 0.0601 |    1008 B |       42.00 |

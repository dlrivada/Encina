```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 108.950 ns |  17.258 ns | 0.9460 ns | 16.08 |    0.24 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   6.776 ns |   1.855 ns | 0.1017 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 447.095 ns |  85.064 ns | 4.6626 ns | 65.99 |    1.05 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 442.362 ns | 135.192 ns | 7.4103 ns | 65.29 |    1.28 | 0.0601 |    1008 B |       42.00 |

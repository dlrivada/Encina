```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   2.657 ns |   0.0534 ns |  0.0029 ns |   0.35 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |   7.670 ns |   2.0452 ns |  0.1121 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 571.133 ns | 187.9983 ns | 10.3048 ns |  74.48 |    1.50 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 865.725 ns |  14.0905 ns |  0.7724 ns | 112.89 |    1.44 | 0.0296 |     504 B |       12.60 |

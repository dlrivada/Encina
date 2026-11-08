```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method               | Mean       | Error     | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   2.662 ns | 0.0025 ns | 0.0034 ns |   0.38 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |   7.024 ns | 0.0766 ns | 0.1123 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 562.237 ns | 1.2762 ns | 1.8706 ns |  80.06 |    1.27 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 847.866 ns | 2.4992 ns | 3.6633 ns | 120.74 |    1.95 | 0.0296 |     504 B |       12.60 |

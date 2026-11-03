```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   2.692 ns |   0.5419 ns |  0.0297 ns |   0.40 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |   6.781 ns |   1.0496 ns |  0.0575 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 548.370 ns | 201.5072 ns | 11.0453 ns |  80.88 |    1.53 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 831.975 ns |  21.8451 ns |  1.1974 ns | 122.71 |    0.91 | 0.0296 |     504 B |       12.60 |

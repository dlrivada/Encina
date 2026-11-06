```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.56GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method               | Mean       | Error     | StdDev    | Median     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|----------:|----------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   2.856 ns | 0.1304 ns | 0.1740 ns |   3.013 ns |   0.42 |    0.03 |      - |         - |        0.00 |
| CreateGtidPosition   |   6.737 ns | 0.0948 ns | 0.1329 ns |   6.669 ns |   1.00 |    0.03 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 549.131 ns | 0.7922 ns | 1.1106 ns | 548.910 ns |  81.54 |    1.57 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 830.139 ns | 2.6923 ns | 3.7742 ns | 828.489 ns | 123.26 |    2.42 | 0.0296 |     504 B |       12.60 |

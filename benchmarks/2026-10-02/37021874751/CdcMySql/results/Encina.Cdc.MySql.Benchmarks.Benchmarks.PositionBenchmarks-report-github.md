```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.42GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   3.238 ns |   2.742 ns |  0.1503 ns |  0.39 |    0.02 |      - |         - |        0.00 |
| CreateGtidPosition   |   8.285 ns |   3.374 ns |  0.1849 ns |  1.00 |    0.03 | 0.0005 |      40 B |        1.00 |
| FromBytes            | 605.024 ns |  63.360 ns |  3.4730 ns | 73.05 |    1.47 | 0.0076 |     688 B |       17.20 |
| ToBytes              | 733.061 ns | 223.400 ns | 12.2453 ns | 88.52 |    2.15 | 0.0057 |     504 B |       12.60 |

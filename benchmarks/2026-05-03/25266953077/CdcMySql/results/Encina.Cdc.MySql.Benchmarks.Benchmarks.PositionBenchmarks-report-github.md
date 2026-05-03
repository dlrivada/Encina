```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host]    : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method               | Mean         | Error     | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|----------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.438 ns | 0.0092 ns |  0.0129 ns |   0.39 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.845 ns | 0.2126 ns |  0.3182 ns |   1.00 |    0.05 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   697.397 ns | 1.6168 ns |  2.2665 ns |  78.94 |    2.82 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,075.330 ns | 9.1533 ns | 13.7003 ns | 121.73 |    4.59 | 0.0286 |     504 B |       12.60 |

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method               | Mean         | Error      | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.438 ns |  0.0086 ns |  0.0123 ns |   0.40 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.495 ns |  0.1373 ns |  0.2055 ns |   1.00 |    0.03 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   693.659 ns |  5.4823 ns |  7.8625 ns |  81.70 |    2.19 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,048.092 ns | 10.7815 ns | 16.1373 ns | 123.45 |    3.54 | 0.0286 |     504 B |       12.60 |

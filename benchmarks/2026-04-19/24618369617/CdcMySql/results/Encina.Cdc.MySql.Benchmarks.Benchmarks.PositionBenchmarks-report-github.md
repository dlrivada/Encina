```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]    : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method               | Mean         | Error      | StdDev     | Median       | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|-----------:|-------------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.972 ns |  0.0027 ns |  0.0039 ns |     3.973 ns |   0.47 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.438 ns |  0.1097 ns |  0.1642 ns |     8.456 ns |   1.00 |    0.03 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   781.033 ns |  5.6236 ns |  8.2430 ns |   783.112 ns |  92.60 |    2.02 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,144.872 ns | 16.7578 ns | 24.5634 ns | 1,160.476 ns | 135.74 |    3.87 | 0.0286 |     504 B |       12.60 |

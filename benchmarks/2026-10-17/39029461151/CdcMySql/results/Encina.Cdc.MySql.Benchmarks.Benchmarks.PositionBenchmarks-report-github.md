```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.07GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   3.911 ns |  0.4878 ns | 0.0267 ns |  0.38 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |  10.315 ns |  0.6123 ns | 0.0336 ns |  1.00 |    0.00 | 0.0005 |      40 B |        1.00 |
| FromBytes            | 775.162 ns | 57.4852 ns | 3.1510 ns | 75.15 |    0.34 | 0.0076 |     688 B |       17.20 |
| ToBytes              | 902.936 ns | 24.4030 ns | 1.3376 ns | 87.54 |    0.27 | 0.0057 |     504 B |       12.60 |

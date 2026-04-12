```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                     | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 22.445 ns | 0.5603 ns | 0.8213 ns | 22.617 ns |  0.81 |    0.03 | 0.0054 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.559 ns | 0.0714 ns | 0.1024 ns |  1.649 ns |  0.06 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 12.162 ns | 0.3504 ns | 0.5136 ns | 12.141 ns |  0.44 |    0.02 | 0.0022 |      56 B |        1.00 |
| CreateChangeMetadata       | 27.610 ns | 0.3547 ns | 0.5308 ns | 27.650 ns |  1.00 |    0.03 | 0.0022 |      56 B |        1.00 |

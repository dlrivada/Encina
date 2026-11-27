```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 20.107 ns | 12.2541 ns | 0.6717 ns |  0.69 |    0.02 | 0.0016 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.412 ns |  0.1924 ns | 0.0105 ns |  0.05 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 11.427 ns |  2.6848 ns | 0.1472 ns |  0.39 |    0.01 | 0.0007 |      56 B |        1.00 |
| CreateChangeMetadata       | 29.210 ns |  8.8128 ns | 0.4831 ns |  1.00 |    0.02 | 0.0007 |      56 B |        1.00 |

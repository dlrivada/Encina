```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 23.110 ns | 15.450 ns | 0.8469 ns |  0.69 |    0.02 | 0.0016 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.540 ns |  1.077 ns | 0.0590 ns |  0.05 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 13.283 ns |  8.696 ns | 0.4766 ns |  0.40 |    0.01 | 0.0007 |      56 B |        1.00 |
| CreateChangeMetadata       | 33.471 ns |  3.513 ns | 0.1925 ns |  1.00 |    0.01 | 0.0007 |      56 B |        1.00 |

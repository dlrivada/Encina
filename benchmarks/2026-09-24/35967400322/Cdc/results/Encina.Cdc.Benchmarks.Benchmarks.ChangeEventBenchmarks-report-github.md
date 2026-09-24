```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 23.194 ns | 20.5741 ns | 1.1277 ns |  0.63 |    0.03 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.483 ns |  0.0861 ns | 0.0047 ns |  0.04 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 13.019 ns |  3.3787 ns | 0.1852 ns |  0.36 |    0.00 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 36.538 ns |  3.4968 ns | 0.1917 ns |  1.00 |    0.01 | 0.0033 |      56 B |        1.00 |

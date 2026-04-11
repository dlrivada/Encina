```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 21.748 ns | 0.7518 ns | 1.1020 ns |  0.62 |    0.03 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.404 ns | 0.0209 ns | 0.0300 ns |  0.04 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 12.172 ns | 0.2399 ns | 0.3591 ns |  0.35 |    0.01 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 34.865 ns | 0.2314 ns | 0.3319 ns |  1.00 |    0.01 | 0.0033 |      56 B |        1.00 |

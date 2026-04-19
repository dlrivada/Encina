```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]    : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 21.426 ns | 0.1710 ns | 0.2560 ns |  0.62 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.392 ns | 0.0012 ns | 0.0016 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 12.300 ns | 0.2509 ns | 0.3756 ns |  0.36 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 34.483 ns | 0.1398 ns | 0.1959 ns |  1.00 | 0.0033 |      56 B |        1.00 |

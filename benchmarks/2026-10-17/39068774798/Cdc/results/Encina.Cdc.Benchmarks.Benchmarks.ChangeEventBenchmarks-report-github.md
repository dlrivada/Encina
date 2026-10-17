```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 21.428 ns | 4.9018 ns | 0.2687 ns |  0.62 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.404 ns | 0.0157 ns | 0.0009 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 13.056 ns | 2.3789 ns | 0.1304 ns |  0.38 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 34.636 ns | 2.1529 ns | 0.1180 ns |  1.00 | 0.0033 |      56 B |        1.00 |

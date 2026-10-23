```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error      | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|-----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 26.842 ns | 10.4454 ns | 0.5725 ns |  0.75 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.387 ns |  0.0253 ns | 0.0014 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 12.836 ns |  1.8452 ns | 0.1011 ns |  0.36 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 35.873 ns |  1.6946 ns | 0.0929 ns |  1.00 | 0.0033 |      56 B |        1.00 |

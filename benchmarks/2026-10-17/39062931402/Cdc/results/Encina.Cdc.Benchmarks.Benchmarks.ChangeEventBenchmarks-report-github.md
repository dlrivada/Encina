```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 23.766 ns | 2.6670 ns | 0.1462 ns |  0.66 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.231 ns | 0.0305 ns | 0.0017 ns |  0.03 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 13.024 ns | 0.4338 ns | 0.0238 ns |  0.36 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 35.980 ns | 2.7366 ns | 0.1500 ns |  1.00 | 0.0033 |      56 B |        1.00 |

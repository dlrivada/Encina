```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 18.002 ns | 0.3717 ns | 0.0204 ns |  0.65 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.014 ns | 0.0351 ns | 0.0019 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 10.419 ns | 1.6707 ns | 0.0916 ns |  0.37 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 27.791 ns | 2.7364 ns | 0.1500 ns |  1.00 | 0.0033 |      56 B |        1.00 |

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean       | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 18.1057 ns | 0.7458 ns | 0.0409 ns |  0.65 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  0.9976 ns | 0.5481 ns | 0.0300 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 10.5963 ns | 3.4875 ns | 0.1912 ns |  0.38 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 28.0357 ns | 1.6951 ns | 0.0929 ns |  1.00 | 0.0033 |      56 B |        1.00 |

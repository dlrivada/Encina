```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean       | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 18.2767 ns | 1.5932 ns | 0.0873 ns |  0.66 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  0.9938 ns | 0.2165 ns | 0.0119 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 10.3451 ns | 1.7453 ns | 0.0957 ns |  0.37 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 27.8080 ns | 1.3413 ns | 0.0735 ns |  1.00 | 0.0033 |      56 B |        1.00 |

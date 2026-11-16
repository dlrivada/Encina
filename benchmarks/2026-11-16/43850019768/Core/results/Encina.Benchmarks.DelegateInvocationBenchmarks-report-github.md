```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     26.80 ns |     0.223 ns |   0.012 ns |     1.25 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     21.42 ns |     1.224 ns |   0.067 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 53,738.73 ns | 5,007.006 ns | 274.451 ns | 2,509.33 |   13.01 | 0.3052 | 0.2441 |    5287 B |       47.21 |
| GenericTypeConstruction |    100.05 ns |     7.312 ns |   0.401 ns |     4.67 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     65.32 ns |    11.658 ns |   0.639 ns |     3.05 |    0.03 | 0.0105 |      - |     176 B |        1.57 |

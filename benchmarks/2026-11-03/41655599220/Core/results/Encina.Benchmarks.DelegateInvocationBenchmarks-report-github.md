```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.11GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error         | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|--------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     37.48 ns |      8.795 ns |   0.482 ns |     1.22 |    0.04 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     30.73 ns |     19.593 ns |   1.074 ns |     1.00 |    0.04 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 90,574.69 ns | 11,242.112 ns | 616.218 ns | 2,950.09 |   92.52 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    130.59 ns |     11.670 ns |   0.640 ns |     4.25 |    0.13 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     81.97 ns |      3.625 ns |   0.199 ns |     2.67 |    0.08 | 0.0105 |      - |     176 B |        1.57 |
